"""Appends a notes fragment (a text file) to this pass's notes.md, so Markdown never passes through a shell string.

    python notes_append.py <fragment file> < /dev/null

Every "{now}" in the fragment is replaced by the clock's HH:MM at the moment of appending, so no time is typed by hand (pass 71: three times
written by estimate on 2026-10-26; section 0, point 10). Write "{now}" for a time that is now; earlier times come from the files, the commits,
or timelog_row.py's rows.
"""
import datetime
import sys
from pathlib import Path

notes = Path(__file__).resolve().parent / "notes.md"
now = datetime.datetime.now().strftime("%H:%M")
fragment = Path(sys.argv[1]).read_text(encoding="utf-8").replace("{now}", now).rstrip("\n") + "\n"
text = notes.read_text(encoding="utf-8")
if not text.endswith("\n"):
    text += "\n"
notes.write_text(text + fragment, encoding="utf-8", newline="\n")
print("appended", len(fragment.splitlines()), "lines at", now)
