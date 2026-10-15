"""Append text (a file's contents) to the time log, keeping LF: timelog_append.py <fragment file>."""
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[3]
LOG = REPO / "src" / "ASL" / "docs" / "ASL Unit Time Log.md"
fragment = Path(sys.argv[1]).read_text(encoding="utf-8")
text = LOG.read_text(encoding="utf-8")
assert "\r\n" not in text
if not text.endswith("\n"):
    text += "\n"
LOG.write_text(text + fragment, encoding="utf-8", newline="")
print("appended", len(fragment), "characters")
