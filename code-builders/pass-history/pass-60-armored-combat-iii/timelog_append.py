"""Append text (a file's contents) to a document, keeping LF: timelog_append.py <fragment file> [document, default the time log]."""
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[3]
DOCS = REPO / "src" / "ASL" / "docs"
target = DOCS / (sys.argv[2] if len(sys.argv) > 2 else "ASL Unit Time Log.md")
fragment = Path(sys.argv[1]).read_text(encoding="utf-8")
text = target.read_text(encoding="utf-8")
assert "\r\n" not in text
if not text.endswith("\n"):
    text += "\n"
target.write_text(text + fragment, encoding="utf-8", newline="")
print("appended", len(fragment), "characters to", target.name)
