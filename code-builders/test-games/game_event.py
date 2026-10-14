"""Print one event of a live game, and the few before it by type. Usage: python event_read.py <game id> <revision> [chars]"""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO, WORK  # noqa: E402
import json
import sys

LIVE = REPO + "/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
events = json.load(open(LIVE + sys.argv[1] + ".game.json", encoding="utf-8"))["events"]
revision = int(sys.argv[2])
limit = int(sys.argv[3]) if len(sys.argv) > 3 else 6000
for event in events:
    if revision - 8 <= event["revision"] < revision:
        print(event["revision"], event["type"], json.dumps(event["payload"])[:200])
    if event["revision"] == revision:
        print(event["revision"], event["type"], event.get("eventId"))
        print(json.dumps(event["payload"], indent=1)[:limit])
