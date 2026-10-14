"""Compare the regenerated chart supplement with the committed one, as CI's jq -S diff does."""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO, WORK  # noqa: E402
import json
import pathlib

committed = pathlib.Path(REPO + "/docs/ASL/SourceRegistry/asl-scenario-a1.supplementary-source-registry.json")
regenerated = pathlib.Path(WORK + "/chart/asl-scenario-a1.supplementary-source-registry.json")
a = json.dumps(json.loads(committed.read_text(encoding="utf-8")), sort_keys=True, indent=1)
b = json.dumps(json.loads(regenerated.read_text(encoding="utf-8")), sort_keys=True, indent=1)
print("chart supplement:", "EQUAL" if a == b else "DIFFERENT")
if a != b:
    import difflib
    print("\n".join(list(difflib.unified_diff(a.split("\n"), b.split("\n"), lineterm=""))[:40]))
