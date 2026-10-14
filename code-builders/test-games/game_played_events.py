"""Read a live game's played events from its file. Usage: python game_read.py <game id> [first revision]"""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO, WORK  # noqa: E402
import json
import sys

LIVE = REPO + "/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
events = json.load(open(LIVE + sys.argv[1] + ".game.json", encoding="utf-8"))["events"]
first = int(sys.argv[2]) if len(sys.argv) > 2 else 0
for event in events:
    if event["revision"] < first:
        continue
    payload = event["payload"]
    kind = event["type"]
    line = f'{event["revision"]:3d} {kind}'
    if kind == "fire-resolved":
        facts = payload.get("facts") or payload.get("attack") or {}
        resolution = payload.get("resolution", {})
        arithmetic = resolution.get("arithmetic", {})
        line += " kind=%s firers=%s" % (facts.get("fireKind"), payload.get("firers"))
        line += "\n      fp=%s column=%s dice=%s drm=%s final=%s result=%s residual=%s" % (
            arithmetic.get("totalFirepower"), arithmetic.get("columnFp"), arithmetic.get("dice"),
            [(d["name"], d["value"]) for d in arithmetic.get("drm", [])], arithmetic.get("finalDr"), arithmetic.get("result"), arithmetic.get("residualFp"))
        line += "\n      firers: " + "; ".join("%s %s x%s" % (f["unitId"], f["firepower"], [m["name"] for m in f.get("multipliers", [])]) for f in arithmetic.get("firers", []))
        for key in ("fireCounter", "firerCounters", "fireCounterUnitIds", "weaponEffects"):
            if resolution.get(key) is not None:
                line += "\n      %s=%s" % (key, json.dumps(resolution[key]))
        extra = {k: v for k, v in facts.items() if k in ("mgBoreSighted", "sprayingFire", "fireLane", "snapShot", "range")}
        line += "\n      facts: %s" % extra
    elif kind == "conditions-changed":
        line += " %s %s" % (payload.get("instance") or payload.get("unit") or payload.get("id"), payload.get("conditions"))
    elif kind in ("residual-fp-placed", "movement-step", "phase-changed", "instance-eliminated", "lineage", "fire-lane-placed", "bore-sighted"):
        line += " " + json.dumps(payload)[:260]
    elif kind == "dice-rolled":
        line += " " + json.dumps(payload)[:160]
    print(line)
