"""The rule inventory's rows, read by the names of its columns, not their positions.

Every documents tool that reads the inventory imports this module, so a column added to the table (the Rules calculator column of
Step 0, 2026-10-28, and those that follow it) or a chapter beyond E changes no tool. A table is one whose header row begins
"| § | Title | Page | Status |"; each of its rows is a dict keyed by the header's names, with "chapter" (the letter of the last
"### Chapter" heading above it, or "ASOP" under "### Appendix 2") and "line" (its index in the file's lines) added.
"""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO  # noqa: E402

PATH = REPO + "/src/ASL/docs/ASL Rule Inventory A to E.md"
# "not audited": a row added from the PDF beyond Chapter E (Step 0, 2026-10-28) that no audit has yet judged against the code.
STATUSES = ["built", "built with a deviation", "partly", "refused", "not built", "not applicable", "not audited"]
IMPLEMENTED = {"built", "built with a deviation", "partly", "refused"}
HEADER_START = "| § | Title | Page | Status |"


def cells(line):
    """A table line's cells, stripped."""
    return [cell.strip() for cell in line.strip()[1:-1].split("|")]


def read_lines(path=PATH):
    """The file's text split into lines, and its line ending."""
    text = open(path, encoding="utf-8", newline="").read()
    nl = "\r\n" if "\r\n" in text else "\n"
    return text.split(nl), nl


def rows(lines):
    """Every rule row of the inventory's tables, in file order. Stops on a row whose cell count differs from its header's."""
    out = []
    names = None
    chapter = None
    for index, line in enumerate(lines):
        if line.startswith("### Chapter "):
            chapter = line[len("### Chapter "):][0]
        elif line.startswith("### Appendix 2"):
            chapter = "ASOP"
        if line.startswith(HEADER_START):
            names = cells(line)
            continue
        if names is None:
            continue
        if not line.startswith("|"):
            names = None
            continue
        if line.startswith("|---"):
            continue
        values = cells(line)
        if len(values) != len(names):
            raise SystemExit(f"line {index + 1}: {len(values)} cells under a header of {len(names)}: {line[:80]}")
        row = dict(zip(names, values))
        if row["Status"] not in STATUSES:
            raise SystemExit(f"line {index + 1}: status {row['Status']!r} is not one of the inventory's")
        row["chapter"] = chapter
        row["line"] = index
        out.append(row)
    return out


def pass_of(row):
    return row.get("Pass or ruling", "")
