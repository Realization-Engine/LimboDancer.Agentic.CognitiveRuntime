"""Appends pass 71's section and its first two rows to the ASL Unit Time Log (times read from the notes' clock reads and file times).

    python timelog_section.py < /dev/null
"""
from pathlib import Path

LOG = Path(__file__).resolve().parents[3] / "src" / "ASL" / "docs" / "ASL Unit Time Log.md"
text = LOG.read_text(encoding="utf-8")
assert "## Pass 71:" not in text
section = (
    "\n## Pass 71: Infantry IV: Snipers and wounds, 2026-10-26\n\n"
    "One session under the user's word of 2026-10-26 (\"Run Pass 71 without my intervention up to the merge gate\"). "
    "The ends of the first rows are the clock reads of the pass's notes (`code-builders/pass-history/pass-71-snipers-wounds/notes.md`); "
    "later rows are written by `timelog_row.py`.\n\n"
    "| Task | Start | End | Duration | Estimate | Note |\n"
    "|---|---|---|---|---|---|\n"
    "| The frame: the prompt and standing rules read, the state checked against main, the branch cut, pass 65's games retired, `base` (08:24 to 08:28), "
    "four read-only frame agents, the design's sections 1 to 7 with R71.1 to R71.17, the build brief and the 4a brief | 08:21 | 08:41 | 0:20 | | "
    "Frame agents 08:25 to about 08:36 |\n"
    "| The base measurement (main's build, alone); the briefs of 4b to 4g | 08:41 | 08:43 | 0:02 | | |\n"
)
if not text.endswith("\n"):
    text += "\n"
LOG.write_text(text + section, encoding="utf-8", newline="\n")
print("ok")
