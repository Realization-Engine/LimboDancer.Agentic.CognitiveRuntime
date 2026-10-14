"""Pass 35, the Studio check of the whole pass: a game with vehicles for one full Player Turn.

Board 3, the German Player Turn from its RPh: a tank in [D9] facing south-east, a halftrack in [D10] facing east with a squad aboard, a squad
in [C10]; a Russian squad with a LMG in the woods of [H10]. Usage: make_check_turn.py game. Only the game-started payload is taken from an
earlier game.
"""
import json
import sys

LIVE = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
GAME = sys.argv[1]
source = json.load(open(LIVE + "p35-rst-cc.game.json", encoding="utf-8"))
started = dict(source["events"][0]["payload"])
started["phasingSide"] = "german"
events = []


def add(kind, payload):
    events.append({
        "eventId": f"{GAME}-{len(events) + 1:04d}", "revision": len(events) + 1, "time": "2026-10-10T12:05:00.0000000Z",
        "source": "map-studio", "type": kind, "visibility": "all", "payload": payload,
    })


CLEAR = {"asl:broken": False, "asl:berserk": False, "asl:captured": False, "asl:melee": False, "asl:ti": False,
         "asl:disrupted": False, "asl:concealed": False, "asl:hidden": False}
PLAIN = {"asl:concealed": False, "asl:hidden": False}
add("game-started", started)
add("instance-created", {"instance": {"id": "tank", "kind": "asl:vehicle", "definition": "attacker-tank", "side": "german",
                                       "position": {"at": "bd03:D9:0", "facing": "south-east"}, "conditions": dict(PLAIN)}})
add("instance-created", {"instance": {"id": "ht", "kind": "asl:vehicle", "definition": "attacker-halftrack", "side": "german",
                                       "position": {"at": "bd03:D10:0", "facing": "east"}, "conditions": dict(PLAIN)}})
add("instance-created", {"instance": {"id": "sq-ht", "kind": "asl:squad", "definition": "attacker-squad", "side": "german",
                                       "position": {"in": "ht", "role": "passenger"}, "conditions": dict(CLEAR)}})
add("instance-created", {"instance": {"id": "sq", "kind": "asl:squad", "definition": "attacker-squad", "side": "german",
                                       "position": {"at": "bd03:C10:0"}, "conditions": dict(CLEAR)}})
add("instance-created", {"instance": {"id": "r1", "kind": "asl:squad", "definition": "defender-squad", "side": "russian",
                                       "position": {"at": "bd03:H10:0"}, "conditions": dict(CLEAR)}})
add("instance-created", {"instance": {"id": "r-lmg", "kind": "asl:mg", "definition": "defender-lmg", "side": "russian",
                                       "holding": {"holder": "r1", "role": "possessed"}, "conditions": {"asl:malfunctioned": False}}})
game = {"schemaVersion": 1, "tenant": source["tenant"], "game": GAME,
        "label": "Pass 35, the Studio check: a tank, a halftrack with a squad, and a squad against a squad in [H10], one Player Turn (board 3)",
        "synthetic": False, "events": events}
with open(LIVE + GAME + ".game.json", "w", encoding="utf-8", newline="") as out:
    json.dump(game, out, indent=2)
    out.write("\n")
print(GAME, len(events))
