"""Pass 35, task 35.6: grain on a LOS in and out of its season (B15.2, B15.6).

A German squad in [FF7] and a Russian squad in [FF10] of board 3, with the grain of [FF8] and [FF9] between them (the
board viewer's LOS tab gives a Hindrance at range 1 and at range 2), at the
German PFPh. p35-grain-nov is played in November, p35-grain-jul in July. Only the game-started payload is taken from an
earlier game; every other event is written here.
"""
import json
import sys

LIVE = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
FROM, TO = (sys.argv[1], sys.argv[2]) if len(sys.argv) > 2 else ("bd03:FF7:0", "bd03:FF10:0")

source = json.load(open(LIVE + "p35-rst-cc.game.json", encoding="utf-8"))
CLEAR = {"asl:broken": False, "asl:berserk": False, "asl:captured": False, "asl:melee": False, "asl:ti": False,
         "asl:disrupted": False, "asl:concealed": False, "asl:hidden": False}

for game, month in (("p35-grain-nov", 11), ("p35-grain-jul", 7)):
    started = dict(source["events"][0]["payload"])
    started["phasingSide"] = "german"
    started["scenarioMonth"] = month
    events = []

    def add(kind, payload):
        events.append({
            "eventId": f"{game}-{len(events) + 1:04d}", "revision": len(events) + 1, "time": "2026-10-09T20:00:00.0000000Z",
            "source": "map-studio", "type": kind, "visibility": "all", "payload": payload,
        })

    add("game-started", started)
    add("instance-created", {"instance": {"id": "g1", "kind": "asl:squad", "definition": "attacker-squad", "side": "german",
                                           "position": {"at": FROM}, "conditions": dict(CLEAR)}})
    add("instance-created", {"instance": {"id": "r1", "kind": "asl:squad", "definition": "defender-squad", "side": "russian",
                                           "position": {"at": TO}, "conditions": dict(CLEAR)}})
    add("phase-changed", {"turn": 1, "phase": "pfph", "phasingSide": "german"})
    doc = {"schemaVersion": 1, "tenant": source["tenant"], "game": game,
           "label": f"Pass 35 task 35.6: a LOS from [FF7] to [FF10] through the grain of [FF8] and [FF9] in month {month} (board 3)",
           "synthetic": False, "events": events}
    with open(LIVE + game + ".game.json", "w", encoding="utf-8", newline="") as out:
        json.dump(doc, out, indent=2)
        out.write("\n")
    print(game, len(events), FROM, TO)
