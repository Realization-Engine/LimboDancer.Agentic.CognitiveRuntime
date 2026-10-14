"""Board 3's building hexes from the oracle's hex facts: the keys of one hex, then each hex with an upper level and its stairwell fact."""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO, WORK  # noqa: E402
import gzip
import json

PATH = REPO + "/src/ASL/tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests/Oracle/bd03.hexfacts.json.gz"
data = json.load(gzip.open(PATH, "rt", encoding="utf-8"))
hexes = data["hexes"] if isinstance(data, dict) and "hexes" in data else data
print(type(data).__name__, list(data.keys())[:10] if isinstance(data, dict) else len(data))
first = hexes[0] if isinstance(hexes, list) else next(iter(hexes.values()))
print(json.dumps(first)[:700])
rows = hexes if isinstance(hexes, list) else list(hexes.values())
for row in rows:
    text = json.dumps(row)
    if "uilding" in text and ("stair" in text.lower() or "evel" in text):
        name = row.get("name") or row.get("hex") or row.get("id")
        keep = {k: v for k, v in row.items() if any(word in k.lower() for word in ("stair", "level", "terrain", "name", "upper"))}
        line = json.dumps(keep)
        if '"stairway": true' in line.lower() or "stair" in line.lower():
            print(name, line[:260])
