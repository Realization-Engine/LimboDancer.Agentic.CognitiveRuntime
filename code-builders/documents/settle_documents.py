"""Settles the conflicts that two claims make in the shared documents when one branch merges main (the Rules-first plan, section 10).

Two claims open at once edit the same documents: the rule inventory (their rows, the claims table, the count tables), the redesign plan's
Rules status lines, the coverage document's count cells, the backlog, and the time log. Their edits are to different rows, but git reports a
conflict whenever two edits touch neighbouring lines. After `git merge origin/main` stops with conflicts on a claim branch (merge, never rebase:
no force push), run

    python settle_documents.py                  settle every conflicted Markdown file, then rewrite the generated parts
    python settle_documents.py --no-generate    settle only (and --repo <path> to work in another checkout)

Each conflict is read three ways (base, this branch, main) and settled when it is one of these:
- both sides added lines at the same place (rows appended to a table, a backlog section, a time log row): both are kept, main's first;
- table rows, matched by their first cell: a row changed on one side takes that side; a row changed on both takes each side's changed cells;
- a generated part (a "Rules status" line, the inventory's count tables in its section 2, its Minigames column, a coverage count cell "N: ..."):
  main's is taken, then rewritten from the rows by pass_rules_status.py, inventory_count_tables.py, coverage_counts.py, and
  minigames_column.py.
Anything else (a cell both sides changed differently, prose both sides rewrote) is left as a conflict, with the usual markers, for a person.
Backlog sections that end up with the same number are renumbered: this branch's moves to the next free number, and the tool names the
references to update. It stages nothing: it prints the paths to add.
"""
import os as _os
import re
import subprocess
import sys
import sys as _sys
import tempfile
_sys.path.insert(0, _os.path.dirname(_os.path.abspath(__file__)))
import inventory_table  # noqa: E402
from paths import REPO, TOOLS  # noqa: E402

STATUS_PREFIX = "> **Rules status**"
INVENTORY = "src/ASL/docs/ASL Rule Inventory A to E.md"
BACKLOG = "src/ASL/docs/ASL Unit Backlog.md"
COUNT_CELL = re.compile(r"^\d+: ")
BACKLOG_HEADING = re.compile(r"^## (\d+)\. (.*)$")
GENERATORS = ["pass_rules_status.py", "inventory_count_tables.py", "coverage_counts.py", "minigames_column.py"]


def git(repo, *args, check=True):
    out = subprocess.run(["git", "-C", repo, *args], capture_output=True, text=True, encoding="utf-8")
    if check and out.returncode != 0:
        sys.exit(f"git {' '.join(args)}: {out.stderr.strip()}")
    return out


def stage(repo, number, path):
    out = git(repo, "show", f":{number}:{path}", check=False)
    return out.stdout if out.returncode == 0 else ""


def merged_with_base(base, ours, theirs):
    """git merge-file's diff3 output for the three texts."""
    with tempfile.TemporaryDirectory() as folder:
        names = []
        for label, text in (("ours", ours), ("base", base), ("theirs", theirs)):
            name = _os.path.join(folder, label)
            with open(name, "w", encoding="utf-8", newline="") as file:
                file.write(text)
            names.append(name)
        out = subprocess.run(["git", "merge-file", "-p", "--diff3", "-L", "ours", "-L", "base", "-L", "theirs", *names],
                             capture_output=True, text=True, encoding="utf-8")
        return out.stdout


def is_row(line):
    return line.startswith("|")


def key(line):
    return inventory_table.cells(line)[0]


class Context:
    """Where a conflict stands: the file, the last "## " heading, and the header of the table it is in."""

    def __init__(self, path):
        self.path, self.heading, self.header = path, "", []

    def follow(self, lines):
        for i, line in enumerate(lines):
            if line.startswith("## "):
                self.heading = line
            if is_row(line) and i + 1 < len(lines) and lines[i + 1].startswith("|---"):
                self.header = inventory_table.cells(line)

    def generated_line(self, line):
        return line.startswith(STATUS_PREFIX) or (self.path == INVENTORY and self.heading.startswith("## 2.") and is_row(line))

    def generated_cell(self, column, values):
        if self.path == INVENTORY and column < len(self.header) and self.header[column] == "Minigames":
            return True
        return all(COUNT_CELL.match(value) for value in values)


def merge_line(context, b, o, t):
    """One line changed on either side; None when both changed it in a way that cannot be settled."""
    if o == t or o == b:
        return t
    if t == b:
        return o
    if context.generated_line(t) and context.generated_line(o):
        return t
    if not (is_row(b) and is_row(o) and is_row(t)):
        return None
    bc, oc, tc = (inventory_table.cells(x) for x in (b, o, t))
    if not len(bc) == len(oc) == len(tc):
        return None
    cells = []
    for i, (x, y, z) in enumerate(zip(bc, oc, tc)):
        if y == z or y == x:
            cells.append(z)
        elif z == x:
            cells.append(y)
        elif context.generated_cell(i, (x, y, z)):
            cells.append(z)
        else:
            return None
    return "| " + " | ".join(cells) + " |"


def merge_rows(context, base, ours, theirs):
    """Table rows matched by their first cell; None when the rows cannot be matched or a row cannot be settled."""
    if not all(is_row(line) for line in base + ours + theirs):
        return None
    sides = [{key(line): line for line in side} for side in (base, ours, theirs)]
    if any(len(index) != len(side) for index, side in zip(sides, (base, ours, theirs))):
        return None
    b, o, t = sides
    result = []
    for k, line in t.items():
        if k in b and k not in o:
            if line != b[k]:
                return None
            continue
        if k not in b and k in o and o[k] != line:
            return None  # both sides added this row, differently
        settled = merge_line(context, b.get(k, line), o.get(k, line), line)
        if settled is None:
            return None
        result.append(settled)
    for k in b:
        if k in o and k not in t and o[k] != b[k]:
            return None  # main removed a row this branch changed
    # This branch's new rows go after the row they follow on this branch, or after main's rows.
    previous = None
    for k, line in o.items():
        if k not in b and k not in t:
            keys = [key(row) for row in result]
            result.insert(keys.index(previous) + 1 if previous in keys else len(result), line)
        previous = k
    return result


def settle_hunk(context, base, ours, theirs):
    if ours == theirs:
        return theirs
    if not base:
        if all(is_row(line) for line in ours + theirs):
            rows = merge_rows(context, [], ours, theirs)
            if rows is not None:
                return rows
        # Keep both whole: a table header or a blank line may stand in both, and each side's copy belongs to its own section.
        while ours and theirs and ours[-1] == "" and theirs[-1] == "":
            ours = ours[:-1]
        return theirs + ([""] if theirs and ours and theirs[-1] != "" and ours[0] != "" and not is_row(ours[0]) else []) + ours
    if len(base) == len(ours) == len(theirs):
        lines = [merge_line(context, b, o, t) for b, o, t in zip(base, ours, theirs)]
        if None not in lines:
            return lines
    return merge_rows(context, base, ours, theirs)


def settle_text(path, base, ours, theirs):
    """The settled text and the number of conflicts left."""
    lines = merged_with_base(base, ours, theirs).split("\n")
    context, out, left, i = Context(path), [], 0, 0
    while i < len(lines):
        if not lines[i].startswith("<<<<<<< "):
            out.append(lines[i])
            i += 1
            continue
        parts, part = {"ours": [], "base": [], "theirs": []}, "ours"
        i += 1
        while not lines[i].startswith(">>>>>>> "):
            if lines[i].startswith("||||||| "):
                part = "base"
            elif lines[i] == "=======":
                part = "theirs"
            else:
                parts[part].append(lines[i])
            i += 1
        i += 1
        context.follow(out)
        settled = settle_hunk(context, parts["base"], parts["ours"], parts["theirs"])
        if settled is None:
            left += 1
            out += ["<<<<<<< ours", *parts["ours"], "=======", *parts["theirs"], ">>>>>>> theirs"]
        else:
            out += settled
    return "\n".join(out), left


def renumber_backlog(text, theirs):
    """Gives this branch's backlog sections that share a number with main's the next free numbers; returns the text and the moves."""
    lines = text.split("\n")
    main_headings = set(theirs.split("\n"))
    numbers = [int(m.group(1)) for line in lines if (m := BACKLOG_HEADING.match(line))]
    seen, moves, free = set(), [], max(numbers, default=0) + 1
    for i, line in enumerate(lines):
        m = BACKLOG_HEADING.match(line)
        if not m:
            continue
        number = int(m.group(1))
        if number in seen and line not in main_headings:
            lines[i] = f"## {free}. {m.group(2)}"
            moves.append((number, free, m.group(2)))
            free += 1
        seen.add(number)
    # Both sides appended, main's first: put the numbered sections back in order (each runs to the next one's heading).
    starts = [i for i, line in enumerate(lines) if BACKLOG_HEADING.match(line)]
    if not starts:
        return "\n".join(lines), moves
    blocks = [lines[a:b] for a, b in zip(starts, starts[1:] + [len(lines)])]
    ending = "\n" if text.endswith("\n") else ""
    trimmed = []
    for block in blocks:
        while block and block[-1] == "":
            block = block[:-1]
        trimmed.append(block)
    trimmed.sort(key=lambda block: int(BACKLOG_HEADING.match(block[0]).group(1)))
    body = "\n\n".join("\n".join(block) for block in trimmed)
    return "\n".join(lines[:starts[0]]) + "\n" + body + ending, moves


def main():
    args = sys.argv[1:]
    repo = args[args.index("--repo") + 1] if "--repo" in args else REPO
    conflicted = [p for p in git(repo, "diff", "--name-only", "--diff-filter=U").stdout.splitlines() if p.endswith(".md")]
    if not conflicted:
        print("no conflicted Markdown file")
        return
    settled, left_total = [], 0
    for path in conflicted:
        base, ours, theirs = stage(repo, 1, path), stage(repo, 2, path), stage(repo, 3, path)
        text, left = settle_text(path, base, ours, theirs)
        moves = []
        if path == BACKLOG:
            text, moves = renumber_backlog(text, theirs)
        with open(_os.path.join(repo, path), "w", encoding="utf-8", newline="") as file:
            file.write(text)
        for old, new, title in moves:
            print(f"{path}: this branch's section {old} ({title}) is now {new}; update the references to backlog section {old} this branch made")
        print(f"{path}: {'settled' if not left else f'{left} conflicts left for a person'}")
        left_total += left
        if not left:
            settled.append(path)
    if "--no-generate" not in args:
        if _os.path.normcase(_os.path.abspath(repo)) != _os.path.normcase(_os.path.abspath(REPO)):
            print("the generators write this checkout only; run them in", repo)
        else:
            for tool in GENERATORS:
                out = subprocess.run([sys.executable, _os.path.join(TOOLS, "documents", tool)], capture_output=True, text=True)
                print(f"{tool}: {(out.stdout.strip().splitlines() or ['(no output)'])[-1]}")
    if settled:
        print("settled; check them, then stage them by path:", " ".join(f'"{p}"' for p in settled))
    sys.exit(1 if left_total else 0)


if __name__ == "__main__":
    main()
