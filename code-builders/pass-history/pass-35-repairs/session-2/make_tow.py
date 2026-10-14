import json, sys

LIVE = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
src = json.load(open(LIVE + "p35-interdict-wreck.game.json", encoding="utf-8"))
started = src["events"][0]["payload"]

events = []


def add(kind, payload):
    events.append({"eventId": f"p35-tow-{len(events) + 1:04d}", "revision": len(events) + 1, "time": "2026-10-09T10:30:00Z",
                   "source": "map-studio", "type": kind, "visibility": "all", "payload": payload})


def vehicle(id, at, facing):
    add("instance-created", {"instance": {"id": id, "kind": "asl:vehicle", "definition": "attacker-halftrack", "side": "german",
                                          "position": {"at": at, "facing": facing},
                                          "conditions": {"asl:concealed": False, "asl:hidden": False, "asl:motion": True}}})


def gun(id, at, facing, holder):
    add("instance-created", {"instance": {"id": id, "kind": "asl:gun", "definition": "attacker-inf-gun", "side": "german",
                                          "holding": {"holder": holder, "role": "towed"}, "conditions": {}}})


add("game-started", started)
vehicle("ht-wall", "bd03:O3:0", sys.argv[1])
gun("gun-wall", "bd03:O3:0", sys.argv[1], "ht-wall")
vehicle("ht-woods", "bd03:D9:0", sys.argv[2])
gun("gun-woods", "bd03:D9:0", sys.argv[2], "ht-woods")
vehicle("ht-free", "bd03:D9:0", sys.argv[2])
add("phase-changed", {"turn": 1, "phase": "pfph", "phasingSide": "german"})
add("phase-changed", {"turn": 1, "phase": "mph", "phasingSide": "german"})

game = {"schemaVersion": 1, "tenant": src["tenant"], "game": "p35-tow2",
        "label": "Pass 35 task 35.12: halftracks towing Guns by the wall of [O3] and the woods of [D9] (board 3)", "synthetic": False, "events": events}
with open(LIVE + "p35-tow2.game.json", "w", encoding="utf-8", newline="") as f:
    json.dump(game, f, indent=2)
    f.write("\n")
print(len(events), "events")
