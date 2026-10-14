import json

LIVE = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
src = json.load(open(LIVE + "p35-interdict-wreck.game.json", encoding="utf-8"))
events = []
INF = {"asl:broken": False, "asl:berserk": False, "asl:captured": False, "asl:melee": False, "asl:ti": False, "asl:disrupted": False,
       "asl:concealed": False, "asl:hidden": False}


def add(kind, payload):
    events.append({"eventId": f"p35-ord-{len(events) + 1:04d}", "revision": len(events) + 1, "time": "2026-10-09T11:00:00Z",
                   "source": "map-studio", "type": kind, "visibility": "all", "payload": payload})


def unit(id, kind, definition, side, at, facing=None, **more):
    position = {"at": at}
    if facing:
        position["facing"] = facing
    conditions = {"asl:concealed": False, "asl:hidden": False} if kind == "asl:vehicle" else dict(INF, **more)
    add("instance-created", {"instance": {"id": id, "kind": kind, "definition": definition, "side": side, "position": position, "conditions": conditions}})


add("game-started", src["events"][0]["payload"])
# The Opportunity Firer: a German crew with its infantry gun in [F10], facing west at the Russian squad in [D10], two hexes off.
unit("g-crew", "asl:crew", "attacker-crew", "german", "bd03:F10:0", **{"asl:bounding-fire": True})
add("instance-created", {"instance": {"id": "g-gun", "kind": "asl:gun", "definition": "attacker-inf-gun", "side": "german",
                                      "position": {"at": "bd03:F10:0", "facing": "west"}, "holding": {"holder": "g-crew", "role": "manned"},
                                      "conditions": {"asl:malfunctioned": False}}})
unit("r-squad", "asl:squad", "defender-squad", "russian", "bd03:D10:0")
# The ATR: a Russian squad in the woods of [P8], a German tank in [P10], two hexes off.
unit("r-atr-squad", "asl:squad", "defender-squad", "russian", "bd03:P8:0")
add("instance-created", {"instance": {"id": "r-atr", "kind": "asl:latw", "definition": "defender-atr", "side": "russian",
                                      "holding": {"holder": "r-atr-squad", "role": "possessed"}, "conditions": {"asl:malfunctioned": False}}})
unit("g-tank", "asl:vehicle", "attacker-tank", "german", "bd03:P10:0", "east")
for phase in ("pfph", "mph", "dfph"):
    add("phase-changed", {"turn": 1, "phase": phase, "phasingSide": "german"})

game = {"schemaVersion": 1, "tenant": src["tenant"], "game": "p35-ordnance",
        "label": "Pass 35 task 35.10: an ATR at two hexes (Case L) and an Opportunity Firer's Gun in the AFPh (board 3)", "synthetic": False, "events": events}
with open(LIVE + "p35-ordnance.game.json", "w", encoding="utf-8", newline="") as f:
    json.dump(game, f, indent=2)
    f.write("\n")
print(len(events), "events")
