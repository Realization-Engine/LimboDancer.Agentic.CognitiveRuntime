"""Pass 40, task 40.7: the games of the corrections that had none (the user's word, 2026-10-11). Board 3.

Usage: python make_407_games.py <game id> <mode>
  assault  A4.61: a German squad in [D10]. It declares Assault Movement, tries a SMOKE grenade in its own Location, and then moves one hex.
  smoke    A4.72: a burning wreck in the woods of [C9]; in [D9] a squad with a MMG (three MF) and a squad with a MMG and a LMG (two MF).
           It is the German APh. The first advances into [C9] (two MF and SMOKE's one): not Difficult Terrain. The second (two MF, all it has): CX.
  board    A4.11, A4.5: a halftrack in [D10] with two leaders; a squad in [E10]. The German MPh.
  bypass   A4.44: a squad on the road in [E6]; a LMG lies in the woods of [F5], which it Bypasses.
"""
import json
import sys

from board_version import version

LIVE = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
GAME, MODE = sys.argv[1], sys.argv[2]

source = json.load(open(LIVE + "p35-rst-cc.game.json", encoding="utf-8"))
started = dict(source["events"][0]["payload"])
blob = version("03")
started["map"] = {"reference": "bd03", "version": blob, "boards": [{"board": "bd03", "version": blob}]}
started["phasingSide"] = "german"
events = []


def add(kind, payload):
    events.append({
        "eventId": f"{GAME}-{len(events) + 1:04d}", "revision": len(events) + 1, "time": "2026-10-12T01:50:00.0000000Z",
        "source": "map-studio", "type": kind, "visibility": "all", "payload": payload,
    })


CLEAR = {"asl:broken": False, "asl:berserk": False, "asl:captured": False, "asl:melee": False, "asl:ti": False,
         "asl:disrupted": False, "asl:concealed": False, "asl:hidden": False}


def unit(identifier, kind, definition, side, at):
    add("instance-created", {"instance": {"id": identifier, "kind": kind, "definition": definition, "side": side,
                                           "position": {"at": at}, "conditions": dict(CLEAR)}})


def mg(identifier, definition, holder=None, at=None):
    instance = {"id": identifier, "kind": "asl:mg", "definition": definition, "side": "german", "conditions": {"asl:malfunctioned": False}}
    if holder:
        instance["holding"] = {"holder": holder, "role": "possessed"}
    else:
        instance["position"] = {"at": at}
    add("instance-created", {"instance": instance})


add("game-started", started)
unit("r-far", "asl:squad", "defender-squad", "russian", "bd03:O3:0")
phases = ("pfph", "mph")
if MODE == "assault":
    unit("g-one", "asl:squad", "attacker-squad", "german", "bd03:D10:0")
    label = "Pass 40 task 40.7: Assault Movement with MF spent inside the Location before its one change of Location (A4.61 EXC; board 3)"
elif MODE == "smoke":
    add("instance-created", {"instance": {"id": "r-tank", "kind": "asl:vehicle", "definition": "defender-tank", "side": "russian",
                                           "position": {"at": "bd03:C9:0", "facing": "east"}, "conditions": {"asl:concealed": False, "asl:hidden": False}}})
    add("vehicle-wrecked", {"id": "r-tank", "burning": True})
    add("instance-created", {"instance": {"id": "r-tank-blaze", "kind": "asl:fire", "position": {"at": "bd03:C9:0"}}})
    unit("g-carry", "asl:squad", "attacker-squad", "german", "bd03:D9:0")
    mg("g-mmg", "attacker-mmg", "g-carry")
    unit("g-heavy", "asl:squad", "attacker-squad", "german", "bd03:D9:0")
    mg("g-mmg-2", "attacker-mmg", "g-heavy")
    mg("g-lmg", "attacker-lmg", "g-heavy")
    phases = ("pfph", "mph", "dfph", "afph", "rtph", "aph")
    label = "Pass 40 task 40.7: an advance into woods that hold SMOKE, by a squad with three MF and by one with two (A4.72; board 3)"
elif MODE == "board":
    add("instance-created", {"instance": {"id": "g-ht", "kind": "asl:vehicle", "definition": "attacker-halftrack", "side": "german",
                                           "position": {"at": "bd03:D10:0", "facing": "east"}, "conditions": {"asl:concealed": False, "asl:hidden": False}}})
    unit("g-leader-far", "asl:leader", "attacker-leader-8-1", "german", "bd03:D10:0")
    unit("g-leader-near", "asl:leader", "attacker-leader-9-1", "german", "bd03:D10:0")
    unit("g-runner", "asl:squad", "attacker-squad", "german", "bd03:E10:0")
    label = "Pass 40 task 40.7: boarding a halftrack on four MF, and not after Double Time (A4.11, A4.5; board 3)"
elif MODE == "bypass":
    unit("g-one", "asl:squad", "attacker-squad", "german", "bd03:E6:0")
    mg("g-lmg", "attacker-lmg", at="bd03:F5:0")
    label = "Pass 40 task 40.7: a squad Bypassing the woods of [F5], where a LMG lies: no Recovery in Bypass (A4.44; board 3)"
else:
    raise SystemExit("mode: assault, smoke, board, or bypass")
for phase in phases:
    add("phase-changed", {"turn": 1, "phase": phase, "phasingSide": "german"})

game = {"schemaVersion": 1, "tenant": source["tenant"], "game": GAME, "label": label, "synthetic": False, "events": events}
with open(LIVE + GAME + ".game.json", "w", encoding="utf-8", newline="") as out:
    json.dump(game, out, indent=2)
    out.write("\n")
print(GAME, len(events))
