"""The key of a reusable baseline: what the baseline's side answers for the games made before a pass depends on nothing else.

The base commit, the properties left out on both sides, the older sweep list, the proof tool's source, and the name and content of every
store game outside the pass's own.

Usage: python baseline_key.py <base commit> <both skip> <older sweep list> <own games, space separated>
"""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import TOOLS, WORK  # noqa: E402
import hashlib
import os
import sys

S = WORK
folder = S + "/store/5a7d1f000000400080000000000057d0"
base, both, sweep = sys.argv[1], sys.argv[2], sys.argv[3]
own = set(sys.argv[4].split()) if len(sys.argv) > 4 else set()
digest = hashlib.sha256()
for part in (base, both, sweep):
    digest.update(part.encode() + b"\0")
with open(TOOLS + "/proof-tool/Program.cs", "rb") as f:
    digest.update(hashlib.sha256(f.read()).digest())
for name in sorted(os.listdir(folder)):
    if name.endswith(".game.json") and name[: -len(".game.json")] not in own:
        with open(folder + "/" + name, "rb") as f:
            digest.update(name.encode() + b"\0" + hashlib.sha256(f.read()).digest())
print(digest.hexdigest()[:16])
