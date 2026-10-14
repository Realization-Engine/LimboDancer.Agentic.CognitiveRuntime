"""Pass 40, task 40.9 (A1.31, A1.32, p. 45; A20.5, p. 88): Deployment and Recombining without a leader. Board 3, no leader on the map.

Usage: python make_409.py <game id> [mode]
  (none)     the German RPh: an Unarmed German squad in [D9]; two Unarmed German HS in [E9]; an armed squad in [F9] and two armed HS in [G9]
  guard      the German PFPh: a German squad in [D9] guards a Russian HS; a second German squad in [F9] guards no one
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
        "eventId": f"{GAME}-{len(events) + 1:04d}", "revision": len(events) + 1, "time": "2026-10-12T04:20:00.0000000Z",
        "source": "map-studio", "type": kind, "visibility": "all", "payload": payload,
    })


CLEAR = {"asl:broken": False, "asl:berserk": False, "asl:captured": False, "asl:melee": False, "asl:ti": False,
         "asl:disrupted": False, "asl:concealed": False, "asl:hidden": False}


def unit(identifier, kind, definition, side, at, **conditions):
    add("instance-created", {"instance": {"id": identifier, "kind": kind, "definition": definition, "side": side,
                                           "position": {"at": at}, "conditions": dict(CLEAR, **conditions)}})


add("game-started", started)
if MODE == "guard":
    unit("g-guard", "asl:squad", "attacker-squad", "german", "bd03:D9:0")
    unit("g-other", "asl:squad", "attacker-squad", "german", "bd03:F9:0")
    unit("r-held", "asl:half-squad", "defender-half-squad", "russian", "bd03:D9:0", **{"asl:captured": True, "asl:unarmed": True})
    add("instance-captured", {"id": "r-held", "custodian": "g-guard"})
    add("phase-changed", {"turn": 1, "phase": "pfph", "phasingSide": "german"})
else:
    unit("g-unarmed", "asl:squad", "attacker-squad", "german", "bd03:D9:0", **{"asl:unarmed": True})
    unit("g-hs-a", "asl:half-squad", "attacker-half-squad", "german", "bd03:E9:0", **{"asl:unarmed": True})
    unit("g-hs-b", "asl:half-squad", "attacker-half-squad", "german", "bd03:E9:0", **{"asl:unarmed": True})
    unit("g-armed", "asl:squad", "attacker-squad", "german", "bd03:F9:0")
    unit("g-hs-c", "asl:half-squad", "attacker-half-squad", "german", "bd03:G9:0")
    unit("g-hs-d", "asl:half-squad", "attacker-half-squad", "german", "bd03:G9:0")
    unit("r-one", "asl:squad", "defender-squad", "russian", "bd03:M9:0")

label = {"": "Pass 40 task 40.9: Unarmed units Recombine and Deploy with no leader; armed ones are refused (A1.31, A1.32; board 3)",
         "guard": "Pass 40 task 40.9: a Guard squad Deploys at will in the PFPh, with no leader and no NTC (A20.5; board 3)"}[MODE]
game = {"schemaVersion": 1, "tenant": source["tenant"], "game": GAME, "label": label, "synthetic": False, "events": events}
with open(LIVE + GAME + ".game.json", "w", encoding="utf-8", newline="") as out:
    json.dump(game, out, indent=2)
    out.write("\n")
print(GAME, len(events))
