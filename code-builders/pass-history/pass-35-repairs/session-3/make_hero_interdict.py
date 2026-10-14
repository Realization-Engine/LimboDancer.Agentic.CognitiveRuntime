"""Pass 35 review fix 6 (A15.23, A10.532): a hero alone with a MG Interdicts at the MG's range.

Board 3: a German hero (or, for the control, an 8-1 leader) alone in [H10] with a MMG; a broken Russian squad in [D10]. [C10] is Open Ground
in a clear LOS from [H10] at range 5 (the board viewer's LOS tab), beyond a hero's own range, on the squad's way to the woods of [C9].
Usage: make_hero_interdict.py [game] [hero|leader]. Only the game-started payload is taken from an earlier game.
"""
import json
import sys

LIVE = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
GAME = sys.argv[1] if len(sys.argv) > 1 else "p35-interdict-hero"
WHO = sys.argv[2] if len(sys.argv) > 2 else "hero"
KIND, DEFINITION, WORDS = (("asl:hero", "attacker-hero", "a hero alone with a MMG Interdicts at its range") if WHO == "hero"
                           else ("asl:leader", "attacker-leader-8-1", "the control: a leader alone with a MMG does not Interdict"))
source = json.load(open(LIVE + "p35-rst-cc.game.json", encoding="utf-8"))
started = dict(source["events"][0]["payload"])
started["phasingSide"] = "german"
events = []


def add(kind, payload):
    events.append({
        "eventId": f"{GAME}-{len(events) + 1:04d}", "revision": len(events) + 1, "time": "2026-10-10T03:40:00.0000000Z",
        "source": "map-studio", "type": kind, "visibility": "all", "payload": payload,
    })


CLEAR = {"asl:broken": False, "asl:berserk": False, "asl:captured": False, "asl:melee": False, "asl:ti": False,
         "asl:disrupted": False, "asl:concealed": False, "asl:hidden": False}
add("game-started", started)
add("instance-created", {"instance": {"id": "g-smc", "kind": KIND, "definition": DEFINITION, "side": "german",
                                       "position": {"at": "bd03:H10:0"}, "conditions": dict(CLEAR)}})
add("instance-created", {"instance": {"id": "g-mmg", "kind": "asl:mg", "definition": "attacker-mmg", "side": "german",
                                       "holding": {"holder": "g-smc", "role": "possessed"}, "conditions": {"asl:malfunctioned": False}}})
add("instance-created", {"instance": {"id": "r-west", "kind": "asl:squad", "definition": "defender-squad", "side": "russian",
                                       "position": {"at": "bd03:D10:0"}, "conditions": dict(CLEAR, **{"asl:broken": True})}})
for phase in ("pfph", "mph", "dfph", "afph", "rtph"):
    add("phase-changed", {"turn": 1, "phase": phase, "phasingSide": "german"})
add("conditions-changed", {"id": "r-west", "conditions": {"asl:dm": True}})
game = {"schemaVersion": 1, "tenant": source["tenant"], "game": GAME,
        "label": f"Pass 35 review fix 6: {WORDS}; from [H10], a rout from [D10] through [C10] at range 5 (board 3)",
        "synthetic": False, "events": events}
with open(LIVE + GAME + ".game.json", "w", encoding="utf-8", newline="") as out:
    json.dump(game, out, indent=2)
    out.write("\n")
print(GAME, len(events))
