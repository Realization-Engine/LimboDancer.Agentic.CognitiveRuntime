"""Pass 35 review fix 7 (A19.12, A20.21, the example on p. 69): a unit that has surrendered before the other side routs does not bar its routes.

Board 3, the German side the ATTACKER: a broken Russian squad in [D9] beside a Good Order German squad in [D10]; a broken German squad under DM
in [C10], whose nearest woods, [C9], are ADJACENT to [D9]. With the Russian squad Disrupted it surrenders at the start of the RtPh and the
German squad may rout to [C9]; not Disrupted (the control) it still stands while the ATTACKER routs, and [C9] is barred (A10.51).
Usage: make_surrender_first.py [game] [disrupted|standing]. Only the game-started payload is taken from an earlier game.
"""
import json
import sys

LIVE = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
GAME = sys.argv[1] if len(sys.argv) > 1 else "p35-surrender-first"
DISRUPTED = (sys.argv[2] if len(sys.argv) > 2 else "disrupted") == "disrupted"
source = json.load(open(LIVE + "p35-rst-cc.game.json", encoding="utf-8"))
started = dict(source["events"][0]["payload"])
started["phasingSide"] = "german"
events = []


def add(kind, payload):
    events.append({
        "eventId": f"{GAME}-{len(events) + 1:04d}", "revision": len(events) + 1, "time": "2026-10-10T04:40:00.0000000Z",
        "source": "map-studio", "type": kind, "visibility": "all", "payload": payload,
    })


CLEAR = {"asl:broken": False, "asl:berserk": False, "asl:captured": False, "asl:melee": False, "asl:ti": False,
         "asl:disrupted": False, "asl:concealed": False, "asl:hidden": False}


def unit(name, definition, side, at, **on):
    add("instance-created", {"instance": {"id": name, "kind": "asl:squad", "definition": definition, "side": side,
                                           "position": {"at": at}, "conditions": dict(CLEAR, **on)}})


add("game-started", started)
unit("g-captor", "attacker-squad", "german", "bd03:D10:0")
unit("g-rout", "attacker-squad", "german", "bd03:C10:0", **{"asl:broken": True})
unit("r-x", "defender-squad", "russian", "bd03:D9:0", **{"asl:broken": True, "asl:disrupted": DISRUPTED})
for phase in ("pfph", "mph", "dfph", "afph", "rtph"):
    add("phase-changed", {"turn": 1, "phase": phase, "phasingSide": "german"})
add("conditions-changed", {"id": "g-rout", "conditions": {"asl:dm": True}})
add("conditions-changed", {"id": "r-x", "conditions": {"asl:dm": True}})
words = ("a Disrupted Russian squad in [D9] has surrendered before the ATTACKER routs, so the German squad in [C10] may rout to [C9] beside it"
         if DISRUPTED else "the control: the Russian squad in [D9] is not Disrupted and still stands, so [C9] beside it is barred to the German squad in [C10]")
game = {"schemaVersion": 1, "tenant": source["tenant"], "game": GAME, "label": f"Pass 35 review fix 7: {words} (board 3)",
        "synthetic": False, "events": events}
with open(LIVE + GAME + ".game.json", "w", encoding="utf-8", newline="") as out:
    json.dump(game, out, indent=2)
    out.write("\n")
print(GAME, len(events))
