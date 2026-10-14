"""Pass 35 review fix 5 (C6.5): a tank that changes its VCA in place loses its Acquisition.

Board 3: a German tank in [D9] facing south-east, a Russian squad in [F10], at the Russian DFPh of Turn 1 so the tank may fire and acquire
as the DEFENDER and then move in its own MPh. Only the game-started payload is taken from an earlier game.
"""
import json
import sys

LIVE = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
GAME = sys.argv[1] if len(sys.argv) > 1 else "p35-vca-acquire"  # p35-vca-keep is the control: the same game played without the VCA change
source = json.load(open(LIVE + "p35-rst-cc.game.json", encoding="utf-8"))
started = dict(source["events"][0]["payload"])
started["phasingSide"] = "russian"
events = []


def add(kind, payload):
    events.append({
        "eventId": f"{GAME}-{len(events) + 1:04d}", "revision": len(events) + 1, "time": "2026-10-10T03:05:00.0000000Z",
        "source": "map-studio", "type": kind, "visibility": "all", "payload": payload,
    })


add("game-started", started)
add("instance-created", {"instance": {"id": "tank", "kind": "asl:vehicle", "definition": "attacker-tank", "side": "german",
                                       "position": {"at": "bd03:D9:0", "facing": "south-east"},
                                       "conditions": {"asl:concealed": False, "asl:hidden": False}}})
add("instance-created", {"instance": {"id": "r1", "kind": "asl:squad", "definition": "defender-squad", "side": "russian",
                                       "position": {"at": "bd03:F10:0"},
                                       "conditions": {"asl:broken": False, "asl:concealed": False, "asl:hidden": False}}})
for phase in ("pfph", "mph", "dfph"):
    add("phase-changed", {"turn": 1, "phase": phase, "phasingSide": "russian"})
game = {"schemaVersion": 1, "tenant": source["tenant"], "game": GAME,
        "label": "Pass 35 review fix 5: a tank in [D9] acquires the squad in [F10], then changes its VCA in its own MPh and loses the Acquisition (board 3)",
        "synthetic": False, "events": events}
with open(LIVE + GAME + ".game.json", "w", encoding="utf-8", newline="") as out:
    json.dump(game, out, indent=2)
    out.write("\n")
print(GAME, len(events))
