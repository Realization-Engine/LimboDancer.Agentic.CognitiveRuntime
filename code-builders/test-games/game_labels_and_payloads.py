"""Labels of games, or the whole payload of named revisions: t3_dump.py labels <game>... | t3_dump.py <game> <rev>..."""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO, WORK  # noqa: E402
import io
import json
import sys

LIVE = REPO + "/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"


def parse(value):
    if isinstance(value, str):
        try:
            return json.loads(value)
        except ValueError:
            return value
    return value


def load(game):
    return json.load(io.open(LIVE + game + ".game.json", encoding="utf-8"))


if sys.argv[1] == "labels":
    for game in sys.argv[2:]:
        data = load(game)
        keys = [key for key in data if key != "events"]
        print("== %s: %s | %s" % (game, data.get("label"), {key: data[key] for key in keys if key not in ("label",) and not isinstance(data[key], (list, dict))}))
        for event in data["events"]:
            if event["type"] == "game-started":
                payload = event["payload"]
                print("   started:", json.dumps({key: value for key, value in payload.items() if key not in ("sides", "map")})[:400])
            if event["type"] == "instance-created":
                payload = event["payload"]
                inst = payload.get("instance") or payload
                print("   unit:", json.dumps(inst)[:330])
else:
    data = load(sys.argv[1])
    revs = set(int(item) for item in sys.argv[2:])
    for event in data["events"]:
        if event["revision"] in revs:
            payload = {key: parse(value) for key, value in event["payload"].items()}
            print("== %d %s" % (event["revision"], event["type"]))
            print(json.dumps(payload, indent=1, ensure_ascii=False))
