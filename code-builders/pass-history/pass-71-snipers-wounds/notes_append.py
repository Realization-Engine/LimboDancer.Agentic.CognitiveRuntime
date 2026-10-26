"""Appends a notes fragment (a text file) to this pass's notes.md, so Markdown never passes through a shell string.

    python notes_append.py <fragment file> < /dev/null
"""
import sys
from pathlib import Path

notes = Path(__file__).resolve().parent / "notes.md"
fragment = Path(sys.argv[1]).read_text(encoding="utf-8").rstrip("\n") + "\n"
text = notes.read_text(encoding="utf-8")
if not text.endswith("\n"):
    text += "\n"
notes.write_text(text + fragment, encoding="utf-8", newline="\n")
print("appended", len(fragment.splitlines()), "lines")
