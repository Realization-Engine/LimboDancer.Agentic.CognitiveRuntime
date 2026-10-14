"""Print the payloads of the named revisions of a live game. Usage: python game_payloads.py <game id> <revision> [<revision> ...]"""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO, WORK  # noqa: E402
import json
import sys

LIVE = REPO + "/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
events = json.load(open(LIVE + sys.argv[1] + ".game.json", encoding="utf-8"))["events"]
wanted = {int(text) for text in sys.argv[2:]}
for event in events:
    if event["revision"] in wanted:
        print(event["revision"], event["type"], json.dumps(event["payload"])[:700].encode("ascii", "replace").decode("ascii"))
