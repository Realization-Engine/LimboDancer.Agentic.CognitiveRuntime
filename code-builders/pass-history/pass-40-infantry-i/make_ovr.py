"""Pass 40, task 40.4 (A4.15 to A4.152, A20.54, p. 49): Infantry OVR. Board 3.

A German squad in the Open Ground of [D9]; a lone Russian leader in the woods of [C9] (TEM +1). Around [C9]: [C8] and [D8] are woods,
[B9], [B10], and [C10] Open Ground. It is the German MPh.

Usage: python make_ovr.py <game id> [mode]
  (none)     the squad alone and the lone leader
  stack      a second squad and a German leader stand with the squad: the leader takes the NTC for all
  unarmed    the Russian unit in [C9] is an Unarmed squad, entered with no NTC to be recaptured (A20.54)
  squad      the Russian unit in [C9] is an armed squad: no OVR (the control)
"""
import json
import sys

from board_version import version

LIVE = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
GAME = sys.argv[1]
MODE = sys.argv[2] if len(sys.argv) > 2 else ""

source = json.load(open(LIVE + "p35-rst-cc.game.json", encoding="utf-8"))
started = dict(source["events"][0]["payload"])
blob = version("03")
started["map"] = {"reference": "bd03", "version": blob, "boards": [{"board": "bd03", "version": blob}]}
started["phasingSide"] = "german"
events = []


def add(kind, payload):
    events.append({
        "eventId": f"{GAME}-{len(events) + 1:04d}", "revision": len(events) + 1, "time": "2026-10-12T02:30:00.0000000Z",
        "source": "map-studio", "type": kind, "visibility": "all", "payload": payload,
    })


CLEAR = {"asl:broken": False, "asl:berserk": False, "asl:captured": False, "asl:melee": False, "asl:ti": False,
         "asl:disrupted": False, "asl:concealed": False, "asl:hidden": False}


def unit(identifier, kind, definition, side, at, **conditions):
    add("instance-created", {"instance": {"id": identifier, "kind": kind, "definition": definition, "side": side,
                                           "position": {"at": at}, "conditions": dict(CLEAR, **conditions)}})


add("game-started", started)
unit("g-one", "asl:squad", "attacker-squad", "german", "bd03:D9:0")
if MODE == "stack":
    unit("g-two", "asl:squad", "attacker-squad", "german", "bd03:D9:0")
    unit("g-leader", "asl:leader", "attacker-leader-8-1", "german", "bd03:D9:0")
if MODE == "unarmed":
    unit("r-unarmed", "asl:squad", "defender-squad", "russian", "bd03:C9:0", **{"asl:unarmed": True})
elif MODE == "squad":
    unit("r-squad", "asl:squad", "defender-squad", "russian", "bd03:C9:0")
else:
    unit("r-leader", "asl:leader", "defender-leader", "russian", "bd03:C9:0")
for phase in ("pfph", "mph"):
    add("phase-changed", {"turn": 1, "phase": phase, "phasingSide": "german"})

label = {"": "Pass 40 task 40.4: an Infantry OVR of a lone leader in the woods of [C9] by a squad from [D9] (A4.15; board 3)",
         "stack": "Pass 40 task 40.4: a leader's NTC for his stack's Infantry OVR of a lone leader in [C9] (A4.15; board 3)",
         "unarmed": "Pass 40 task 40.4: an Unarmed squad in [C9] entered and recaptured in the MPh (A20.54; board 3)",
         "squad": "Pass 40 task 40.4 control: an armed squad in [C9] is no Infantry OVR's target (A4.14, A4.15; board 3)"}[MODE]
game = {"schemaVersion": 1, "tenant": source["tenant"], "game": GAME, "label": label, "synthetic": False, "events": events}
with open(LIVE + GAME + ".game.json", "w", encoding="utf-8", newline="") as out:
    json.dump(game, out, indent=2)
    out.write("\n")
print(GAME, len(events))
