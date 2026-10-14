"""Pass 35, third session: the vehicle Close Combat panel's stale selection.

Two T-34s, each in Close Combat with German squads, in two Locations of board 3 ([D10] with two squads, [E10] with one),
at the Russian CCPh. Only the game-started payload is taken from the earlier game; every other event is written here.
"""
import json
import sys

LIVE = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
GAME = sys.argv[1] if len(sys.argv) > 1 else "p35-cc-panel"

source = json.load(open(LIVE + "p35-rst-cc.game.json", encoding="utf-8"))
started = source["events"][0]["payload"]
assert source["events"][0]["type"] == "game-started"

events = []


def add(kind, payload):
    events.append({
        "eventId": f"{GAME}-{len(events) + 1:04d}", "revision": len(events) + 1, "time": "2026-10-09T18:00:00.0000000Z",
        "source": "map-studio", "type": kind, "visibility": "all", "payload": payload,
    })


SQUAD = {"asl:broken": False, "asl:berserk": False, "asl:captured": False, "asl:melee": False, "asl:ti": False,
         "asl:disrupted": False, "asl:concealed": False, "asl:hidden": False}
TANK = {"asl:concealed": False, "asl:hidden": False}

add("game-started", started)
for tank, at in (("t-d10", "bd03:D10:0"), ("t-e10", "bd03:E10:0")):
    add("instance-created", {"instance": {"id": tank, "kind": "asl:vehicle", "definition": "defender-tank", "side": "russian",
                                           "position": {"at": at, "facing": "east"}, "conditions": dict(TANK)}})
for squad, at in (("g1", "bd03:D10:0"), ("g2", "bd03:D10:0"), ("g3", "bd03:E10:0")):
    add("instance-created", {"instance": {"id": squad, "kind": "asl:squad", "definition": "attacker-squad", "side": "german",
                                           "position": {"at": at}, "conditions": dict(SQUAD)}})
for phase in ("pfph", "mph", "dfph", "afph", "rtph", "aph", "ccph"):
    add("phase-changed", {"turn": 1, "phase": phase, "phasingSide": "russian"})

game = {"schemaVersion": 1, "tenant": source["tenant"], "game": GAME,
        "label": "Pass 35: the vehicle CC panel's shared draft: two T-34s in Close Combat in [D10] (two squads) and [E10] (one), board 3",
        "synthetic": False, "events": events}
with open(LIVE + GAME + ".game.json", "w", encoding="utf-8", newline="") as out:
    json.dump(game, out, indent=2)
    out.write("\n")
print(GAME, len(events))
