"""List a live game's events, and print the fire attacks' DRM and overstack facts. Usage: python game_events.py <game id>"""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO, WORK  # noqa: E402
import json
import sys

LIVE = REPO + "/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
events = json.load(open(LIVE + sys.argv[1] + ".game.json", encoding="utf-8"))["events"]
for event in events:
    payload = event["payload"]
    line = f'{event["revision"]:3d} {event["type"]}'
    if event["type"] == "fire-resolved":
        attack = payload.get("attack", {})
        drm = payload.get("resolution", {}).get("arithmetic", {}).get("drm", [])
        line += f' firerOverstack={attack.get("firerOverstack")} targetOverstack={attack.get("targetOverstack")} drm={[(item["name"], item["value"]) for item in drm]}'
    if event["type"] == "movement-step":
        line += f' {payload.get("movers") or payload.get("units")} to {payload.get("to")} halfMf={payload.get("halfMf")}'
    if event["type"] == "advanced":
        line += " " + json.dumps(payload)[:200]
    print(line)
