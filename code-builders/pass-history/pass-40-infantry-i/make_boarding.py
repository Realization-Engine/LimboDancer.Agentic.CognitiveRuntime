"""Pass 40, task 40.7 (A4.11, p. 48): a SMC that mounts a conveyance has four MF, not six.

Board 3. A German halftrack in [D10] with two leaders and four unpossessed LMGs in its hex. The page offers boarding only when no Infantry
move is open, so the MF are spent in place: each failed Recovery attempt costs one MF (A4.44). The leader that has spent four may not board;
the one that has spent two may.

Usage: python make_boarding.py [game id]
"""
import json
import sys

from board_version import version

LIVE = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
GAME = sys.argv[1] if len(sys.argv) > 1 else "p40-boarding"

source = json.load(open(LIVE + "p35-rst-cc.game.json", encoding="utf-8"))
started = dict(source["events"][0]["payload"])
blob = version("03")
started["map"] = {"reference": "bd03", "version": blob, "boards": [{"board": "bd03", "version": blob}]}
started["phasingSide"] = "german"
events = []


def add(kind, payload):
    events.append({
        "eventId": f"{GAME}-{len(events) + 1:04d}", "revision": len(events) + 1, "time": "2026-10-12T02:00:00.0000000Z",
        "source": "map-studio", "type": kind, "visibility": "all", "payload": payload,
    })


CLEAR = {"asl:broken": False, "asl:berserk": False, "asl:captured": False, "asl:melee": False, "asl:ti": False,
         "asl:disrupted": False, "asl:concealed": False, "asl:hidden": False}

add("game-started", started)
add("instance-created", {"instance": {"id": "r-far", "kind": "asl:squad", "definition": "defender-squad", "side": "russian",
                                       "position": {"at": "bd03:O3:0"}, "conditions": dict(CLEAR)}})
add("instance-created", {"instance": {"id": "g-ht", "kind": "asl:vehicle", "definition": "attacker-halftrack", "side": "german",
                                       "position": {"at": "bd03:D10:0", "facing": "east"}, "conditions": {"asl:concealed": False, "asl:hidden": False}}})
for identifier, definition in (("g-leader-four", "attacker-leader-8-1"), ("g-leader-two", "attacker-leader-9-1")):
    add("instance-created", {"instance": {"id": identifier, "kind": "asl:leader", "definition": definition, "side": "german",
                                           "position": {"at": "bd03:D10:0"}, "conditions": dict(CLEAR)}})
for number in range(1, 5):
    add("instance-created", {"instance": {"id": f"g-lmg-{number}", "kind": "asl:mg", "definition": "attacker-lmg", "side": "german",
                                           "position": {"at": "bd03:D10:0"}, "conditions": {"asl:malfunctioned": False}}})
for phase in ("pfph", "mph"):
    add("phase-changed", {"turn": 1, "phase": phase, "phasingSide": "german"})

game = {"schemaVersion": 1, "tenant": source["tenant"], "game": GAME,
        "label": "Pass 40 task 40.7: two leaders beside a halftrack in [D10], with four LMGs on the ground: boarding on four MF (A4.11; board 3)",
        "synthetic": False, "events": events}
with open(LIVE + GAME + ".game.json", "w", encoding="utf-8", newline="") as out:
    json.dump(game, out, indent=2)
    out.write("\n")
print(GAME, len(events))
