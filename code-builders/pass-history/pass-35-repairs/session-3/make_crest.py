"""Pass 35 review fix 2 (B1.14, B10.31): a rout up a hill across the Crest Line the Interdictor's LOS crosses.

Board 3: a broken Russian squad on the road in [E6] (level 0), a German squad in the woods of [E7] behind it. The squad may rout up into the
level 1 hill hex [E5], across the hexside the German LOS crosses, and on. Only the game-started payload is taken from an earlier game.
"""
import json
import sys

LIVE = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
GAME = sys.argv[1] if len(sys.argv) > 1 else "p35-crest"
RUSSIAN = sys.argv[2] if len(sys.argv) > 2 else "bd03:E6:0"
GERMAN = sys.argv[3] if len(sys.argv) > 3 else "bd03:E7:0"

source = json.load(open(LIVE + "p35-rst-cc.game.json", encoding="utf-8"))
started = dict(source["events"][0]["payload"])
started["phasingSide"] = "german"
events = []


def add(kind, payload):
    events.append({
        "eventId": f"{GAME}-{len(events) + 1:04d}", "revision": len(events) + 1, "time": "2026-10-09T23:20:00.0000000Z",
        "source": "map-studio", "type": kind, "visibility": "all", "payload": payload,
    })


CLEAR = {"asl:broken": False, "asl:berserk": False, "asl:captured": False, "asl:melee": False, "asl:ti": False,
         "asl:disrupted": False, "asl:concealed": False, "asl:hidden": False}

add("game-started", started)
add("instance-created", {"instance": {"id": "r-one", "kind": "asl:squad", "definition": "defender-squad", "side": "russian",
                                       "position": {"at": RUSSIAN}, "conditions": dict(CLEAR, **{"asl:broken": True})}})
add("instance-created", {"instance": {"id": "g-one", "kind": "asl:squad", "definition": "attacker-squad", "side": "german",
                                       "position": {"at": GERMAN}, "conditions": dict(CLEAR)}})
for phase in ("pfph", "mph", "dfph", "afph", "rtph"):
    add("phase-changed", {"turn": 1, "phase": phase, "phasingSide": "german"})
add("conditions-changed", {"id": "r-one", "conditions": {"asl:dm": True}})

game = {"schemaVersion": 1, "tenant": source["tenant"], "game": GAME,
        "label": f"Pass 35 review fix 2: a broken squad in {RUSSIAN} routs up the hill across the Crest Line a German LOS from {GERMAN} crosses (board 3)",
        "synthetic": False, "events": events}
with open(LIVE + GAME + ".game.json", "w", encoding="utf-8", newline="") as out:
    json.dump(game, out, indent=2)
    out.write("\n")
print(GAME, len(events))
