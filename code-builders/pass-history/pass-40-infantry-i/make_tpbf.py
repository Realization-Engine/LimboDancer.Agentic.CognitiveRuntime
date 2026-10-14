"""Pass 40, task 40.5 (A8.311, A8.312, p. 61): the attack the DEFENDER owes in its own Location, and a crew's FP in FPF. Board 3.

Usage: python make_tpbf.py <game id> [mode]
  (none)     a berserk German squad in the Open Ground of [D9]; a Russian squad in the woods of [C9]. The German MPh.
  two        a second Russian squad in [C9], already marked First Fire: the two cannot fire as one group
  broken     the Russian squad in [C9] is broken: no attack is owed (the control)
  crew       a Russian crew mans an AT Gun in [D10] beside a squad marked Final Fire; a German squad in [F10] and a berserk one in [F9].
             The German MPh: the Gun fires at the first squad, and the berserk one then charges into [D10] (a crew's FP in FPF)
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
        "eventId": f"{GAME}-{len(events) + 1:04d}", "revision": len(events) + 1, "time": "2026-10-12T03:40:00.0000000Z",
        "source": "map-studio", "type": kind, "visibility": "all", "payload": payload,
    })


CLEAR = {"asl:broken": False, "asl:berserk": False, "asl:captured": False, "asl:melee": False, "asl:ti": False,
         "asl:disrupted": False, "asl:concealed": False, "asl:hidden": False}


def unit(identifier, kind, definition, side, at, **conditions):
    add("instance-created", {"instance": {"id": identifier, "kind": kind, "definition": definition, "side": side,
                                           "position": {"at": at}, "conditions": dict(CLEAR, **conditions)}})


add("game-started", started)
if MODE == "crew":
    unit("r-crew", "asl:crew", "defender-crew", "russian", "bd03:D10:0")
    add("instance-created", {"instance": {"id": "r-gun", "kind": "asl:gun", "definition": "defender-at-gun", "side": "russian",
                                           "position": {"at": "bd03:D10:0", "facing": "east"}, "holding": {"holder": "r-crew", "role": "manned"},
                                           "conditions": {"asl:malfunctioned": False}}})
    unit("r-one", "asl:squad", "defender-squad", "russian", "bd03:D10:0", **{"asl:final-fire": True})
    unit("g-one", "asl:squad", "attacker-squad", "german", "bd03:F10:0")
    unit("g-two", "asl:squad", "attacker-squad", "german", "bd03:F9:0", **{"asl:berserk": True})
else:
    unit("g-one", "asl:squad", "attacker-squad", "german", "bd03:D9:0", **{"asl:berserk": True})
    unit("r-one", "asl:squad", "defender-squad", "russian", "bd03:C9:0", **({"asl:broken": True} if MODE == "broken" else {}))
    if MODE == "two":
        unit("r-two", "asl:squad", "defender-squad", "russian", "bd03:C9:0", **{"asl:first-fire": True})
for phase in ("pfph", "mph"):
    add("phase-changed", {"turn": 1, "phase": phase, "phasingSide": "german"})

label = {"": "Pass 40 task 40.5: a berserk squad charges from [D9] into a Russian squad's woods of [C9]; the attack owed (A8.312; board 3)",
         "two": "Pass 40 task 40.5: two Russian squads in [C9], one marked First Fire, owe the attack on a berserk squad's entry (A8.312; board 3)",
         "broken": "Pass 40 task 40.5 control: a broken squad in [C9] owes no attack on a berserk squad's entry (A8.312; board 3)",
         "crew": "Pass 40 task 40.5: a Russian crew at its AT Gun and a squad marked Final Fire in [D10], a berserk squad's charge; a crew's FP in FPF (A8.311; board 3)"}[MODE]
game = {"schemaVersion": 1, "tenant": source["tenant"], "game": GAME, "label": label, "synthetic": False, "events": events}
with open(LIVE + GAME + ".game.json", "w", encoding="utf-8", newline="") as out:
    json.dump(game, out, indent=2)
    out.write("\n")
print(GAME, len(events))
