"""Report CR, BOM, and the final newline of the documents the pass's closing touches."""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO, WORK  # noqa: E402
import io

D = REPO + "/src/ASL/docs/"
for name in ["Passes/ASL Unit Backlog Pass 55 Design.md", "ASL Unit Backlog Passes Plan.md", "ASL Unit Backlog.md", "ASL Rule Inventory A to E.md",
             "ASL Rule Coverage.md", "ASL Card Play and Map Studio Redesign Plan.md", "Passes/Scenario A1 Backlog Pass 55 Review 2026-10-13.md"]:
    try:
        raw = io.open(D + name, "rb").read()
    except FileNotFoundError:
        print(name, "absent")
        continue
    print(name, "CR", raw.count(b"\r"), "LF", raw.count(b"\n"), "BOM", raw.startswith(b"\xef\xbb\xbf"), "ends with LF", raw.endswith(b"\n"), "em-dash", raw.decode("utf-8").count("\u2014"))
