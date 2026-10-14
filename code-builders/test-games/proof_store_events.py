"""Print the type and a slice of the payload of a range of revisions of a game in the proof store.

Usage: python store_events.py <game id> <first revision> <last revision>
"""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO, WORK  # noqa: E402
import json
import sys

STORE = WORK + "/store/5a7d1f000000400080000000000057d0/"
events = json.load(open(STORE + sys.argv[1] + ".game.json", encoding="utf-8"))["events"]
for event in events:
    if int(sys.argv[2]) <= event["revision"] <= int(sys.argv[3]):
        print(event["revision"], event["type"], json.dumps(event["payload"])[:260].encode("ascii", "replace").decode("ascii"))
