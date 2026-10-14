"""The recorded shots and fire of live games, one line each: shots.py <game id>..."""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO, WORK  # noqa: E402
import io
import json
import sys

LIVE = REPO + "/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"


def parse(value):
    return json.loads(value) if isinstance(value, str) else value


for game in sys.argv[1:]:
    data = json.load(io.open(LIVE + game + ".game.json", encoding="utf-8"))
    print("== %s: %d events" % (game, len(data["events"])))
    for event in data["events"]:
        kind, payload = event["type"], event["payload"]
        if kind == "ordnance-fired":
            facts, res = parse(payload.get("facts")) or {}, parse(payload.get("resolution")) or {}
            hit = res.get("toHit") or {}
            gun = res.get("gun") or {}
            kill = res.get("kill") or {}
            print("  %d ordnance %s ammo=%s elite=%s ha=%s canister=%s | dice=%s drm=%s final=%s hit=%s | use=%s | gun: malf=%s rofKept=%s counter=%s acq=%s | kill: %s tk=%s %s | %s" % (
                event["revision"], payload.get("gun"), facts.get("ammunition"), facts.get("elite"), facts.get("heightAdvantage"),
                json.dumps(facts.get("canister")) if facts.get("canister") else None,
                hit.get("dice"), [(m.get("name"), m.get("value")) for m in hit.get("drm") or []], hit.get("finalDr"), hit.get("hit"),
                res.get("ammunitionUse"), gun.get("malfunctioned"), gun.get("rateOfFireKept"), gun.get("fireCounter"), gun.get("acquisition"),
                kill.get("basicTk"), kill.get("finalTk"), kill.get("result"), res.get("disposition")))
        elif kind == "fire-resolved":
            res = parse(payload.get("resolution")) or {}
            arith = res.get("arithmetic") or {}
            print("  %d fire at %s fp=%s drm=%s | %s" % (event["revision"], payload.get("target") or payload.get("location"),
                                                    arith.get("totalFirepower"), [(m.get("name"), m.get("value")) for m in arith.get("drm") or []],
                                                    [(d.get("unitId"), d.get("finalDr"), d.get("result")) for d in res.get("defending") or []]))
        elif kind not in ("game-started", "instance-created", "phase-changed", "rolled", "roll-drawn", "dice-rolled"):
            print("  %d %s %s" % (event["revision"], kind, json.dumps(payload)[:160]))
