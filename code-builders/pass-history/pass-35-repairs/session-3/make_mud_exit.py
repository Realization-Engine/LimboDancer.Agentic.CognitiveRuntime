"""Pass 35 review fix 4 (E3.6, D2.16): a tank in Motion leaves the map by the dirt road of [Q10] on board 3, in Mud and in the dry.

Board 3's only road is Dirt Road (the board viewer's legend); it crosses the south edge at [Q10]. Two games: p35-mud-exit with weather:mud,
p35-dry-exit without. Only the game-started payload is taken from an earlier game.

A Recalled AFV leaves by its Friendly Board Edge (the left, for the German side), where board 3 has no road; the exit cost is the same member.
"""
import json

LIVE = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
source = json.load(open(LIVE + "p35-rst-cc.game.json", encoding="utf-8"))

for game_id, rules, words in (("p35-mud-exit", ["weather:mud"], "in Mud: the Open Ground cost and the Mud MP, no road rate"),
                              ("p35-dry-exit", [], "in the dry: the road rate")):
    started = dict(source["events"][0]["payload"])
    started["phasingSide"] = "german"
    started["specialRules"] = rules
    events = []

    def add(kind, payload):
        events.append({
            "eventId": f"{game_id}-{len(events) + 1:04d}", "revision": len(events) + 1, "time": "2026-10-10T01:50:00.0000000Z",
            "source": "map-studio", "type": kind, "visibility": "all", "payload": payload,
        })

    add("game-started", started)
    add("instance-created", {"instance": {"id": "tank", "kind": "asl:vehicle", "definition": "attacker-tank", "side": "german",
                                           "position": {"at": "bd03:Q10:0", "facing": "south-east"},
                                           "conditions": {"asl:concealed": False, "asl:hidden": False, "asl:motion": True}}})
    add("instance-created", {"instance": {"id": "r1", "kind": "asl:squad", "definition": "defender-squad", "side": "russian",
                                           "position": {"at": "bd03:O3:0"},
                                           "conditions": {"asl:broken": False, "asl:concealed": False, "asl:hidden": False}}})
    for phase in ("pfph", "mph"):
        add("phase-changed", {"turn": 1, "phase": phase, "phasingSide": "german"})
    game = {"schemaVersion": 1, "tenant": source["tenant"], "game": game_id,
            "label": f"Pass 35 review fix 4: a tank in Motion leaves by the dirt road of [Q10] {words} (board 3)",
            "synthetic": False, "events": events}
    with open(LIVE + game_id + ".game.json", "w", encoding="utf-8", newline="") as out:
        json.dump(game, out, indent=2)
        out.write("\n")
    print(game_id, len(events))
