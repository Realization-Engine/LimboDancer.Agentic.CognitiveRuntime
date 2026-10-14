"""Pass 35 review fix 3 (D8.4, C.8): a bogged halftrack unloads its Passenger as its first MP of the MPh, under an AT Gun.

Board 3: the halftrack bogged in [D9] with a squad aboard, a Russian AT Gun and crew in [F10] facing west, at the German MPh of Turn 1.
Only the game-started payload is taken from an earlier game.
"""
import json
import sys

LIVE = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
GAME = sys.argv[1] if len(sys.argv) > 1 else "p35-bog-unload"  # p35-ord-draft: the same game, for the ordnance draft after a kill

source = json.load(open(LIVE + "p35-rst-cc.game.json", encoding="utf-8"))
started = dict(source["events"][0]["payload"])
started["phasingSide"] = "german"
events = []


def add(kind, payload):
    events.append({
        "eventId": f"{GAME}-{len(events) + 1:04d}", "revision": len(events) + 1, "time": "2026-10-09T23:30:00.0000000Z",
        "source": "map-studio", "type": kind, "visibility": "all", "payload": payload,
    })


CLEAR = {"asl:broken": False, "asl:berserk": False, "asl:captured": False, "asl:melee": False, "asl:ti": False,
         "asl:disrupted": False, "asl:concealed": False, "asl:hidden": False}

add("game-started", started)
add("instance-created", {"instance": {"id": "ht-bog", "kind": "asl:vehicle", "definition": "attacker-halftrack", "side": "german",
                                       "position": {"at": "bd03:D9:0", "facing": "south-east"}, "conditions": {"asl:concealed": False, "asl:hidden": False, "asl:bogged": True}}})
add("instance-created", {"instance": {"id": "sq-bog", "kind": "asl:squad", "definition": "attacker-squad", "side": "german",
                                       "position": {"in": "ht-bog", "role": "passenger"}, "conditions": dict(CLEAR)}})
add("instance-created", {"instance": {"id": "r-crew", "kind": "asl:crew", "definition": "defender-crew", "side": "russian",
                                       "position": {"at": "bd03:F10:0"}, "conditions": dict(CLEAR)}})
add("instance-created", {"instance": {"id": "r-gun", "kind": "asl:gun", "definition": "defender-at-gun", "side": "russian",
                                       "position": {"at": "bd03:F10:0", "facing": "west"}, "holding": {"holder": "r-crew", "role": "manned"},
                                       "conditions": {"asl:malfunctioned": False}}})
for phase in ("pfph", "mph"):
    add("phase-changed", {"turn": 1, "phase": phase, "phasingSide": "german"})

game = {"schemaVersion": 1, "tenant": source["tenant"], "game": GAME,
        "label": "Pass 35 review fix 3: a bogged halftrack in [D9] unloads under an AT Gun in [F10]; the shot takes no Case J (board 3)",
        "synthetic": False, "events": events}
with open(LIVE + GAME + ".game.json", "w", encoding="utf-8", newline="") as out:
    json.dump(game, out, indent=2)
    out.write("\n")
print(GAME, len(events))
