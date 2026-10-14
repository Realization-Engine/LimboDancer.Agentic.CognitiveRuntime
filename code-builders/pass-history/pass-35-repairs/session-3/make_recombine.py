"""Pass 35, task 35.16: the Recombine review named the new squad by its identifier.

Two German HS and an 8-1 leader in [D10] of board 3, a Russian squad in [P10], at the German RPh of Turn 1.
Only the game-started payload is taken from an earlier game; every other event is written here.
"""
import json

LIVE = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
GAME = "p35-recombine"

source = json.load(open(LIVE + "p35-rst-cc.game.json", encoding="utf-8"))
started = dict(source["events"][0]["payload"])
started["phasingSide"] = "german"

events = []


def add(kind, payload):
    events.append({
        "eventId": f"{GAME}-{len(events) + 1:04d}", "revision": len(events) + 1, "time": "2026-10-09T19:00:00.0000000Z",
        "source": "map-studio", "type": kind, "visibility": "all", "payload": payload,
    })


CLEAR = {"asl:broken": False, "asl:berserk": False, "asl:captured": False, "asl:melee": False, "asl:ti": False,
         "asl:disrupted": False, "asl:concealed": False, "asl:hidden": False}

add("game-started", started)
for unit, kind, definition, side, at in (
        ("h1", "asl:half-squad", "attacker-half-squad", "german", "bd03:D10:0"),
        ("h2", "asl:half-squad", "attacker-half-squad", "german", "bd03:D10:0"),
        ("gl", "asl:leader", "attacker-leader-8-1", "german", "bd03:D10:0"),
        ("r1", "asl:squad", "defender-squad", "russian", "bd03:P10:0")):
    add("instance-created", {"instance": {"id": unit, "kind": kind, "definition": definition, "side": side,
                                           "position": {"at": at}, "conditions": dict(CLEAR)}})

game = {"schemaVersion": 1, "tenant": source["tenant"], "game": GAME,
        "label": "Pass 35 task 35.16: two German HS and a leader in [D10] Recombine in the RPh (board 3)",
        "synthetic": False, "events": events}
with open(LIVE + GAME + ".game.json", "w", encoding="utf-8", newline="") as out:
    json.dump(game, out, indent=2)
    out.write("\n")
print(GAME, len(events))
