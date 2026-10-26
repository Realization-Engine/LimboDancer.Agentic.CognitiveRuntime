"""Removes the frame agents' raw estimate totals from the design and the notes (the user, 2026-10-26: "Get rid of that entirely").

    python drop_raw_totals.py < /dev/null
"""
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
design = ROOT / "src" / "ASL" / "docs" / "Passes" / "ASL Unit Backlog Pass 71 Design.md"
notes = Path(__file__).resolve().parent / "notes.md"

text = design.read_text(encoding="utf-8")
footnote = ("\n\n(The frame agents' raw estimates, before calibration: 4a 5:45, 4b 4:30, 4c 4:00, 4d 5:30, 4e 1:35, 4f 2:45, 4g 2:30, a build of 26:35. "
            "They are kept for the calibration only; pass 70's ran about four times its build.)")
assert text.count(footnote) == 1
design.write_text(text.replace(footnote, ""), encoding="utf-8", newline="\n")

text = notes.read_text(encoding="utf-8")
old = " The agents' estimates sum to about 28:45 against the plan's 1:20; the table gives 26:35 after the moves."
assert text.count(old) == 1, "notes"
text = text.replace(old, "")
assert not re.search(r"26:35|28:45", text)
notes.write_text(text, encoding="utf-8", newline="\n")
print("ok")
