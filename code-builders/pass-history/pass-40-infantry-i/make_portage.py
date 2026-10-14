"""Pass 40, task 40.7 (A4.4, A4.41, A4.42, p. 50): portage.

Board 3. A German squad in the Open Ground of [D10] possesses a MMG (4 PP, so it has three MF, as the example of A4.42 has it); a second
German squad stands in [D9]; a Russian squad in [F10] sees both hexes. It is the German MPh.

In the game: the first squad carries the MMG to [D9] and drops it; its MF do not come back. The second squad Recovers the MMG and may not
carry it on in this phase. In the AFPh the MMG, having moved, does not fire.

Usage: python make_portage.py [game id] [leader]
  leader: a second squad with a second MMG stands in [D10] with a leader, for the leader's IPC as a choice (A4.42).
"""
import json
import sys

from board_version import version

LIVE = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
GAME = sys.argv[1] if len(sys.argv) > 1 else "p40-portage"
LEADER = len(sys.argv) > 2 and sys.argv[2] == "leader"

source = json.load(open(LIVE + "p35-rst-cc.game.json", encoding="utf-8"))
started = dict(source["events"][0]["payload"])
blob = version("03")
started["map"] = {"reference": "bd03", "version": blob, "boards": [{"board": "bd03", "version": blob}]}
started["phasingSide"] = "german"
events = []


def add(kind, payload):
    events.append({
        "eventId": f"{GAME}-{len(events) + 1:04d}", "revision": len(events) + 1, "time": "2026-10-12T01:30:00.0000000Z",
        "source": "map-studio", "type": kind, "visibility": "all", "payload": payload,
    })


CLEAR = {"asl:broken": False, "asl:berserk": False, "asl:captured": False, "asl:melee": False, "asl:ti": False,
         "asl:disrupted": False, "asl:concealed": False, "asl:hidden": False}


def unit(identifier, kind, definition, side, at):
    add("instance-created", {"instance": {"id": identifier, "kind": kind, "definition": definition, "side": side,
                                           "position": {"at": at}, "conditions": dict(CLEAR)}})


def mmg(identifier, holder):
    add("instance-created", {"instance": {"id": identifier, "kind": "asl:mg", "definition": "attacker-mmg", "side": "german",
                                           "holding": {"holder": holder, "role": "possessed"}, "conditions": {"asl:malfunctioned": False}}})


add("game-started", started)
unit("g-carry", "asl:squad", "attacker-squad", "german", "bd03:D10:0")
mmg("g-mmg", "g-carry")
if LEADER:
    unit("g-carry-2", "asl:squad", "attacker-squad", "german", "bd03:D10:0")
    mmg("g-mmg-2", "g-carry-2")
    unit("g-leader", "asl:leader", "attacker-leader-8-1", "german", "bd03:D10:0")
else:
    unit("g-two", "asl:squad", "attacker-squad", "german", "bd03:D9:0")
unit("r-one", "asl:squad", "defender-squad", "russian", "bd03:F10:0")
for phase in ("pfph", "mph"):
    add("phase-changed", {"turn": 1, "phase": phase, "phasingSide": "german"})

label = ("Pass 40 task 40.7: two squads, each with a MMG, and a leader in [D10]: the leader's IPC as the mover's choice (A4.42; board 3)" if LEADER
         else "Pass 40 task 40.7: a squad with a MMG in [D10], a second squad in [D9]: no recouped portage, no second portage, no AFPh fire by a moved MMG (A4.4, A4.41; board 3)")
game = {"schemaVersion": 1, "tenant": source["tenant"], "game": GAME, "label": label, "synthetic": False, "events": events}
with open(LIVE + GAME + ".game.json", "w", encoding="utf-8", newline="") as out:
    json.dump(game, out, indent=2)
    out.write("\n")
print(GAME, len(events))
